using System.Threading;
using Cysharp.Threading.Tasks;

namespace Cast.Game
{
    public sealed class RevealBooster : IBooster
    {
        public BoosterType Type => BoosterType.Reveal;
        public bool RequiresTarget => false;

        public bool CanUse(IGameSession session) =>
            session != null && session.Phase == GamePhase.Playing;

        public UniTask<BoosterResult> UseAsync(BoosterController controller, CancellationToken ct)
        {
            bool revealed = controller.Session.RandomReveal();
            BoosterResult result = revealed ? BoosterResult.Ok(Type) : BoosterResult.Rejected(Type, "no unrevealed cats");
            return UniTask.FromResult(result);
        }
    }
}
