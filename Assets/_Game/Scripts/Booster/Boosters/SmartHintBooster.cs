using System.Collections.Generic;
using System.Threading;
using CaskFramework.Core;
using CaskFramework.UI;
using Cysharp.Threading.Tasks;

namespace Cast.Game
{

    public sealed class SmartHintBooster : IBooster
    {
        public BoosterType Type => BoosterType.Hint;
        public bool RequiresTarget => false;

        public bool CanUse(IGameSession session) =>
            session != null && session.Phase == GamePhase.Playing;

        public async UniTask<BoosterResult> UseAsync(BoosterController controller, CancellationToken ct)
        {
            List<(int row, int col)> cells = controller.Session.GetHintCells();
            
            var ui = GameRuntime.Get<IUIManager>();
            var boardView = controller.Board;

            if (boardView != null) boardView.SetOverlay(true);

            foreach (var (row, col) in cells)
            {
                var cellView = boardView.GetCell(row, col);
                if (cellView != null)
                    cellView.SetSortingLayer("UI");
            }

            PopupBoosterHint popup = null;
            await ui.PushPopupAsync<PopupBoosterHint>(UIConst.PopupBoosterHint, onLoad: (_, p) => popup = p);

            if (popup != null)
                await popup.WaitForConfirmAsync();

            await ui.PopPopupAsync();

            foreach (var (row, col) in cells)
            {
                var cellView = boardView.GetCell(row, col);
                if (cellView != null)
                    cellView.SetSortingLayer("Gameplay");
            }
            if (boardView != null) boardView.SetOverlay(false);

            if (cells.Count == 0)
                return BoosterResult.Rejected(Type, "not enough unrevealed cats or no valid hint cells");

            return BoosterResult.Ok(Type);
        }
    }
}
