using System;
using System.Collections.Generic;
using CaskFramework.UI;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Cast.Game
{
    public sealed class SmartHintBooster : IBooster
    {
        public BoosterType Type => BoosterType.Hint;
        public bool RequiresTarget => false;

        public bool CanUse(IGameSession session) =>
            session != null && session.Phase == GamePhase.Playing;

        public void Execute(BoosterController controller, Action<BoosterResult> onDone)
        {
            HintResult hint = controller.Session.GetHint();
            if (hint == null)
            {
                onDone(BoosterResult.Rejected(Type, "no valid hint available"));
                return;
            }

            RunAsync(controller, hint, onDone).Forget();
        }

        private async UniTaskVoid RunAsync(BoosterController controller, HintResult hint, Action<BoosterResult> onDone)
        {
            IUIManager ui = controller.Ui;
            BoardView boardView = controller.Board;
            IBoardInput input = controller.Input;

            if (boardView != null) boardView.SetOverlay(true);
            foreach (var (row, col) in hint.Cells)
                boardView?.GetCell(row, col)?.SetSortingLayer("UI");

            var ghosts = new HintPreviewGhosts(boardView);

            void RevertBoard()
            {
                ghosts.ClearAll();
                foreach (var (row, col) in hint.Cells)
                    boardView?.GetCell(row, col)?.SetSortingLayer("Gameplay");
                if (boardView != null) boardView.SetOverlay(false);
                input?.EndHintPreview();
            }

            input?.BeginHintPreview(
                hint.Cells,
                onTap: (r, c) => ghosts.ToggleHint(r, c),
                onDoubleTap: (r, c) => ghosts.SetReveal(r, c));

            PopupBoosterHint popup = null;
            await ui.PushPopupAsync<PopupBoosterHint>(UIConst.PopupBoosterHint, onLoad: (_, p) => popup = p);

            if (popup == null)
            {
                RevertBoard();
                await ui.PopPopupAsync();
                onDone(BoosterResult.Ok(Type));
                return;
            }

            Color swatch = ResolveColor(controller.Session.Board, hint.ColorIndex);
            popup.Show(hint.Message, swatch);

            var completion = new UniTaskCompletionSource<bool>();
            popup.SetConfirmCallback(() => completion.TrySetResult(popup.Applied));

            bool applied = await completion.Task;

            List<(int Row, int Col)> ghostHints = ghosts.GetGhostHints();
            (int Row, int Col)? ghostReveal = ghosts.GetGhostReveal();

            RevertBoard();

            if (popup.Applied)
                await ui.PopPopupAsync();

            if (applied)
                ApplyGhosts(controller.Session, ghostHints, ghostReveal);

            onDone(BoosterResult.Ok(Type));
        }

        private static void ApplyGhosts(IGameSession session, List<(int Row, int Col)> ghostHints, (int Row, int Col)? ghostReveal)
        {
            foreach (var (row, col) in ghostHints)
                session.SetHint(row, col, true);

            if (ghostReveal.HasValue)
                session.Reveal(ghostReveal.Value.Row, ghostReveal.Value.Col);
        }

        private static Color ResolveColor(BoardState board, sbyte colorIndex)
        {
            Color[] colors = board.Level.Colors;
            if (colors != null && colorIndex >= 0 && colorIndex < colors.Length)
                return colors[colorIndex];
            return Color.white;
        }
    }

    public sealed class HintPreviewGhosts
    {
        private readonly BoardView _board;
        private readonly HashSet<(int, int)> _ghostHints = new HashSet<(int, int)>();
        private (int, int)? _ghostReveal;

        public HintPreviewGhosts(BoardView board)
        {
            _board = board;
        }

        public void ToggleHint(int row, int col)
        {
            if (_ghostReveal.HasValue && _ghostReveal.Value == (row, col))
                return;

            if (_ghostHints.Remove((row, col)))
            {
                _board?.GetCell(row, col)?.SetGhostHint(false);
                return;
            }

            _ghostHints.Add((row, col));
            _board?.GetCell(row, col)?.SetGhostHint(true);
        }

        public void SetReveal(int row, int col)
        {
            if (_ghostReveal.HasValue && _ghostReveal.Value == (row, col))
            {
                _ghostReveal = null;
                _board?.GetCell(row, col)?.ClearGhost();
                return;
            }

            if (_ghostReveal.HasValue)
            {
                var (pr, pc) = _ghostReveal.Value;
                _board?.GetCell(pr, pc)?.ClearGhost();
            }

            if (_ghostHints.Remove((row, col)))
                _board?.GetCell(row, col)?.SetGhostHint(false);

            _ghostReveal = (row, col);
            _board?.GetCell(row, col)?.SetGhostReveal(true);
        }

        public List<(int Row, int Col)> GetGhostHints()
        {
            var list = new List<(int Row, int Col)>();
            foreach (var cell in _ghostHints)
                list.Add(cell);
            return list;
        }

        public (int Row, int Col)? GetGhostReveal() => _ghostReveal;

        public void ClearAll()
        {
            foreach (var (row, col) in _ghostHints)
                _board?.GetCell(row, col)?.ClearGhost();
            _ghostHints.Clear();

            if (_ghostReveal.HasValue)
            {
                var (row, col) = _ghostReveal.Value;
                _board?.GetCell(row, col)?.ClearGhost();
                _ghostReveal = null;
            }
        }
    }
}
