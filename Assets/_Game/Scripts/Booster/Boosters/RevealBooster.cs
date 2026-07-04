using System;

namespace Cast.Game
{
    public sealed class RevealBooster : IBooster
    {
        public BoosterType Type => BoosterType.Reveal;
        public bool RequiresTarget => false;

        public bool CanUse(IGameSession session) =>
            session != null && session.Phase == GamePhase.Playing;

        public void Execute(BoosterController controller, Action<BoosterResult> onDone)
        {
            bool revealed = controller.Session.RandomReveal();
            onDone(revealed ? BoosterResult.Ok(Type) : BoosterResult.Rejected(Type, "no unrevealed cats"));
        }
    }
}
