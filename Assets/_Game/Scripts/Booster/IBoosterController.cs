using System;

namespace Cast.Game
{
    public interface IBoosterController
    {
        bool IsBusy { get; }
        IBoosterInventory Inventory { get; }

        void Use(BoosterType type, Action<BoosterResult> onDone = null);

        event Action<BoosterType> BoosterStarted;
        event Action<BoosterResult> BoosterFinished;
    }
}
