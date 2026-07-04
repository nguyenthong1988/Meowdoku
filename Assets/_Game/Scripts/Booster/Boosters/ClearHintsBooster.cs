using System;

namespace Cast.Game
{
    public sealed class ClearHintsBooster : IBooster
    {
        public BoosterType Type => BoosterType.ClearHints;
        public bool RequiresTarget => false;

        public bool CanUse(IGameSession session) =>
            session != null && session.Phase == GamePhase.Playing;

        public void Execute(BoosterController controller, Action<BoosterResult> onDone)
        {
            bool applied = controller.Session.ClearAllHints();
            onDone(applied ? BoosterResult.Ok(Type) : BoosterResult.Rejected(Type, "no hints to clear"));
        }
    }
}
