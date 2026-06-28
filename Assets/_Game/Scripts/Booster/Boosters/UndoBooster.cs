using System.Threading;

using Cysharp.Threading.Tasks;

namespace Cast.Game
{

    public sealed class UndoBooster : IBooster
    {
        public BoosterType Type => BoosterType.Undo;
        public bool RequiresTarget => false;

        public bool CanUse(IGameSession session) =>
            session != null && session.Phase == GamePhase.Playing && session.Hearts < session.HeartsMax;

        public async UniTask<BoosterResult> UseAsync(BoosterController ctx, CancellationToken ct)
        {
            bool applied = ctx.Session.UndoWrong();
            await UniTask.CompletedTask;
            return applied ? BoosterResult.Ok(Type) : BoosterResult.Rejected(Type, "no wrong move to undo");
        }
    }
}
