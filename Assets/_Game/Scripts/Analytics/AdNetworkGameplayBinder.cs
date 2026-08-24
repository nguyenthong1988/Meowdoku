using System;
using CaskFramework.Profile;

namespace Cast.Game
{
    public sealed class AdNetworkGameplayBinder : IDisposable
    {
        private const string CoinBalanceKey = "Coin";

        private readonly IBoosterController _boosters;
        private readonly IProfileService _profile;

        private Action<BoosterResult> _onBoosterFinished;
        private Action<string, int> _onBalanceChanged;

        private bool _bound;
        private int? _lastCoinBalance;

        public AdNetworkGameplayBinder(IBoosterController boosters, IProfileService profile)
        {
            _boosters = boosters;
            _profile = profile;
        }

        public void Bind()
        {
            if (_bound) return;
            _bound = true;

            _onBoosterFinished = OnBoosterFinished;
            _onBalanceChanged = OnBalanceChanged;

            if (_boosters != null)
                _boosters.BoosterFinished += _onBoosterFinished;

            if (_profile != null)
                _profile.OnBalanceChanged += _onBalanceChanged;
        }

        public void Dispose()
        {
            if (!_bound) return;
            _bound = false;

            if (_boosters != null)
                _boosters.BoosterFinished -= _onBoosterFinished;

            if (_profile != null)
                _profile.OnBalanceChanged -= _onBalanceChanged;
        }

        private void OnBoosterFinished(BoosterResult result)
        {
            if (!result.Applied) return;
            AdNetworkTracker.Track(AdNetworkEvents.use_prop);
        }

        private void OnBalanceChanged(string key, int newBalance)
        {
            if (key != CoinBalanceKey) return;

            if (_lastCoinBalance.HasValue)
            {
                int delta = newBalance - _lastCoinBalance.Value;
                if (delta != 0)
                    AdNetworkTracker.TrackVirtualResourceTransaction(delta, ResourceType.Coins);
            }

            _lastCoinBalance = newBalance;
        }
    }
}
