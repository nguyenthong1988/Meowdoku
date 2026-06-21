using System;

namespace Cast.Game.Booster
{

    public interface IBoosterInventory
    {
        int Count(BoosterType type);
        bool TrySpend(BoosterType type);
        void Grant(BoosterType type, int amount);

        event Action<BoosterType, int> CountChanged;
    }
}
