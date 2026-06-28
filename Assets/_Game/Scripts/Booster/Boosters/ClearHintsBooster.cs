using System.Threading;

using Cysharp.Threading.Tasks;

namespace Cast.Game
{

    public sealed class ClearHintsBooster : IBooster
    {
        public BoosterType Type => BoosterType.ClearHints;
        public bool RequiresTarget => false;

        public bool CanUse(IGameSession session) =>
            session != null && session.Phase == GamePhase.Playing;

        public async UniTask<BoosterResult> UseAsync(BoosterController ctx, CancellationToken ct)
        {
            bool applied = ctx.Session.ClearAllHints();
            await UniTask.CompletedTask;
            return applied ? BoosterResult.Ok(Type) : BoosterResult.Rejected(Type, "no hints to clear");
        }
    }
}
