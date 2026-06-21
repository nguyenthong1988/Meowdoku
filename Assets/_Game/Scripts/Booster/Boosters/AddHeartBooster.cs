using System.Threading;
using Cast.Game.Gameplay;
using Cysharp.Threading.Tasks;

namespace Cast.Game.Booster
{

    public sealed class AddHeartBooster : IBooster
    {
        public BoosterType Type => BoosterType.AddHeart;
        public bool RequiresTarget => false;

        public bool CanUse(IGameSession session) =>
            session != null && session.Phase == GamePhase.Playing && session.Hearts < session.HeartsMax;

        public async UniTask<BoosterResult> UseAsync(BoosterContext ctx, CancellationToken ct)
        {
            bool added = ctx.Session.AddHeart();
            
            await UniTask.CompletedTask;
            return added ? BoosterResult.Ok(Type) : BoosterResult.Rejected(Type, "hearts full");
        }
    }
}
