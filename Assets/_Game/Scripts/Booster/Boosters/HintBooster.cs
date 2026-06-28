using System.Threading;

using Cysharp.Threading.Tasks;

namespace Cast.Game
{

    public sealed class HintBooster : IBooster
    {
        public BoosterType Type => BoosterType.Hint;
        public bool RequiresTarget => false;

        public bool CanUse(IGameSession session) =>
            session != null && session.Phase == GamePhase.Playing;

        public async UniTask<BoosterResult> UseAsync(BoosterController ctx, CancellationToken ct)
        {
            
            bool applied = ctx.Session.Hint();
            await UniTask.CompletedTask;
            return applied ? BoosterResult.Ok(Type) : BoosterResult.Rejected(Type, "no hint available");
        }
    }
}
