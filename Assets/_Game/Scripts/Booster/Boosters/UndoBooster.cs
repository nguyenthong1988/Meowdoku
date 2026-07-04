using System;

namespace Cast.Game
{
    public sealed class UndoBooster : IBooster
    {
        public BoosterType Type => BoosterType.Undo;
        public bool RequiresTarget => false;

        public bool CanUse(IGameSession session) =>
            session != null && session.Phase == GamePhase.Playing && session.Hearts < session.HeartsMax;

        public void Execute(BoosterController controller, Action<BoosterResult> onDone)
        {
            bool applied = controller.Session.UndoWrong();
            onDone(applied ? BoosterResult.Ok(Type) : BoosterResult.Rejected(Type, "no wrong move to undo"));
        }
    }
}
