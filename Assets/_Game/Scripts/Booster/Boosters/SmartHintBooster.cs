using System.Collections.Generic;
using System.Threading;

using Cysharp.Threading.Tasks;

namespace Cast.Game
{

    public sealed class SmartHintBooster : IBooster
    {
        public BoosterType Type => BoosterType.Hint;
        public bool RequiresTarget => false;

        public bool CanUse(IGameSession session) =>
            session != null && session.Phase == GamePhase.Playing;

        public async UniTask<BoosterResult> UseAsync(BoosterController ctx, CancellationToken ct)
        {
            List<(int row, int col)> cells = ctx.Session.GetHintCells();
            await UniTask.CompletedTask;

            if (cells.Count == 0)
                return BoosterResult.Rejected(Type, "not enough unrevealed cats or no valid hint cells");

            return BoosterResult.Ok(Type);
        }
    }
}
