using System.Threading;
using Cast.Game.Gameplay;
using Cysharp.Threading.Tasks;

namespace Cast.Game.Booster
{

    public sealed class HintBooster : IBooster
    {
        public BoosterType Type => BoosterType.Hint;
        public bool RequiresTarget => false;

        public bool CanUse(IGameSession session) =>
            session != null && session.Phase == GamePhase.Playing;

        public async UniTask<BoosterResult> UseAsync(BoosterContext ctx, CancellationToken ct)
        {
            
            bool applied = ctx.Session.Hint();
            await UniTask.CompletedTask;
            return applied ? BoosterResult.Ok(Type) : BoosterResult.Rejected(Type, "no hint available");
        }
    }
}
