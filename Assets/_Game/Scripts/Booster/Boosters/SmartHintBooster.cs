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

            List<(int Row, int Col)> involvedCells = CollectInvolvedCells(hint);

            if (boardView != null) boardView.SetOverlay(true);
            foreach (var (row, col) in involvedCells)
                boardView?.GetCell(row, col)?.SetSortingLayer("UI");

            ShowPreview(boardView, hint);

            void RevertBoard()
            {
                ClearPreview(boardView, hint);
                foreach (var (row, col) in involvedCells)
                    boardView?.GetCell(row, col)?.SetSortingLayer("Gameplay");
                if (boardView != null) boardView.SetOverlay(false);
            }

            PopupBoosterHint popup = null;
            await ui.PushPopupAsync<PopupBoosterHint>(UIConst.PopupBoosterHint, onLoad: (_, p) => popup = p);

            if (popup == null)
            {
                RevertBoard();
                await ui.PopPopupAsync();
                onDone(BoosterResult.Cancel(Type));
                return;
            }

            Color swatch = ResolveColor(controller.Session.Board, hint.ColorIndex);
            popup.Show(hint.Message, swatch);

            var completion = new UniTaskCompletionSource<bool>();
            popup.SetConfirmCallback(() => completion.TrySetResult(popup.Applied));

            bool applied = await completion.Task;

            RevertBoard();

            if (popup.Applied)
                await ui.PopPopupAsync();

            if (applied)
                Apply(controller.Session, hint);

            onDone(applied ? BoosterResult.Ok(Type) : BoosterResult.Cancel(Type));
        }

        private static List<(int Row, int Col)> CollectInvolvedCells(HintResult hint)
        {
            var seen = new HashSet<(int, int)>();
            var cells = new List<(int Row, int Col)>();

            void Add((int Row, int Col) cell)
            {
                if (seen.Add(cell)) cells.Add(cell);
            }

            foreach (var cell in hint.FocusCells) Add(cell);
            foreach (var cell in hint.ExcludeCells) Add(cell);
            foreach (var cell in hint.VictimCells) Add(cell);
            foreach (var cell in hint.ChainCells) Add(cell);
            if (hint.PlaceCell.HasValue) Add(hint.PlaceCell.Value);
            if (hint.RemoveMarkCell.HasValue) Add(hint.RemoveMarkCell.Value);
            if (hint.TrialCell.HasValue) Add(hint.TrialCell.Value);

            return cells;
        }

        private static void ShowPreview(BoardView boardView, HintResult hint)
        {
            if (boardView == null) return;

            foreach (var (row, col) in hint.ExcludeCells)
            {
                if (hint.TrialCell.HasValue && hint.TrialCell.Value == (row, col)) continue;
                boardView.GetCell(row, col)?.SetGhostHint(true);
            }

            foreach (var (row, col) in hint.ChainCells)
                boardView.GetCell(row, col)?.SetGhostReveal(true);

            if (hint.TrialCell.HasValue)
                boardView.GetCell(hint.TrialCell.Value.Row, hint.TrialCell.Value.Col)?.SetGhostReveal(true);

            if (hint.PlaceCell.HasValue)
                boardView.GetCell(hint.PlaceCell.Value.Row, hint.PlaceCell.Value.Col)?.SetGhostReveal(true);
        }

        private static void ClearPreview(BoardView boardView, HintResult hint)
        {
            if (boardView == null) return;

            foreach (var (row, col) in hint.ExcludeCells)
                boardView.GetCell(row, col)?.ClearGhost();

            foreach (var (row, col) in hint.ChainCells)
                boardView.GetCell(row, col)?.ClearGhost();

            if (hint.TrialCell.HasValue)
                boardView.GetCell(hint.TrialCell.Value.Row, hint.TrialCell.Value.Col)?.ClearGhost();

            if (hint.PlaceCell.HasValue)
                boardView.GetCell(hint.PlaceCell.Value.Row, hint.PlaceCell.Value.Col)?.ClearGhost();
        }

        private static void Apply(IGameSession session, HintResult hint)
        {
            if (hint.RemoveMarkCell.HasValue)
                session.SetHint(hint.RemoveMarkCell.Value.Row, hint.RemoveMarkCell.Value.Col, false);

            foreach (var (row, col) in hint.ExcludeCells)
                session.SetHint(row, col, true);

            if (hint.PlaceCell.HasValue)
                session.Reveal(hint.PlaceCell.Value.Row, hint.PlaceCell.Value.Col);
        }

        private static Color ResolveColor(BoardState board, sbyte colorIndex)
        {
            Color[] colors = board.Level.Colors;
            if (colors != null && colorIndex >= 0 && colorIndex < colors.Length)
                return colors[colorIndex];
            return Color.white;
        }
    }
}
