using System;

namespace Cast.Game
{
    public interface IBooster
    {
        BoosterType Type { get; }
        bool RequiresTarget { get; }

        bool CanUse(IGameSession session);

        void Execute(BoosterController controller, Action<BoosterResult> onDone);
    }
}
