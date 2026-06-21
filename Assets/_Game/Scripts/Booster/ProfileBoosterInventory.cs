using System;
using CaskFramework.Profile;

namespace Cast.Game.Booster
{

    public sealed class ProfileBoosterInventory : IBoosterInventory
    {
        private readonly IProfileService _profile;

        public event Action<BoosterType, int> CountChanged;

        public ProfileBoosterInventory(IProfileService profile)
        {
            _profile = profile ?? throw new ArgumentNullException(nameof(profile));
            _profile.OnBalanceChanged += OnBalanceChanged;
        }

        public int Count(BoosterType type) => (int)_profile.GetBalance(type);

        public bool TrySpend(BoosterType type) => _profile.Sink(type, 1);

        public void Grant(BoosterType type, int amount) => _profile.Source(type, amount);

        public void Dispose()
        {
            _profile.OnBalanceChanged -= OnBalanceChanged;
            CountChanged = null;
        }

        private void OnBalanceChanged(string key, int newBalance)
        {
            
            foreach (BoosterType type in (BoosterType[])Enum.GetValues(typeof(BoosterType)))
            {
                if (ProfileKey.Is(key, type))
                {
                    CountChanged?.Invoke(type, (int)newBalance);
                    return;
                }
            }
        }
    }
}
