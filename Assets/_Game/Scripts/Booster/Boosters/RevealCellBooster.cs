using System.Collections.Generic;
using System.Threading;
using Cast.Game.Data;
using Cast.Game.Gameplay;
using Cysharp.Threading.Tasks;

namespace Cast.Game.Booster
{

    public sealed class RevealCellBooster : IBooster
    {
        public BoosterType Type => BoosterType.RevealCell;
        public bool RequiresTarget => true;

        public bool CanUse(IGameSession session) =>
            session != null && session.Phase == GamePhase.Playing;

        public async UniTask<BoosterResult> UseAsync(BoosterContext ctx, CancellationToken ct)
        {
            (bool ok, int row, int col) = await ctx.Targeting.PickCellAsync(ct);
            if (!ok)
                return BoosterResult.Cancel(Type);

            IGameSession session = ctx.Session;
            int regionColor = session.Board.Level.GetCell(row, col).ColorIndex;

            IReadOnlyList<CatPlacement> solution = session.Level.Solution;
            for (int i = 0; i < solution.Count; i++)
            {
                CatPlacement p = solution[i];
                if (p.ColorIndex != regionColor) continue;
                session.Reveal(p.Row, p.Col);
                return BoosterResult.Ok(Type);
            }

            return BoosterResult.Rejected(Type, "no cat for that region");
        }
    }
}
