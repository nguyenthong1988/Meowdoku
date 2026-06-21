using System;
using System.Collections.Generic;
using System.Threading;
using Cast.Game.Board;
using Cast.Game.Gameplay;
using Cysharp.Threading.Tasks;

namespace Cast.Game.Booster
{

    public sealed class BoosterService : IBoosterService
    {
        private readonly BoosterContext _ctx;
        private readonly IBoardInputGate _gate;
        private readonly IBoosterInventory _inventory;
        private readonly Dictionary<BoosterType, IBooster> _boosters = new Dictionary<BoosterType, IBooster>();

        public bool IsBusy { get; private set; }

        public event Action<BoosterType> BoosterStarted;
        public event Action<BoosterResult> BoosterFinished;

        public BoosterService(IGameSession session, IBoardInputGate gate, IBoardTargeting targeting,
                              BoardView board, IBoosterInventory inventory, params IBooster[] boosters)
        {
            _gate = gate ?? throw new ArgumentNullException(nameof(gate));
            _inventory = inventory ?? throw new ArgumentNullException(nameof(inventory));
            _ctx = new BoosterContext(session, board, targeting);

            if (boosters != null)
                foreach (IBooster b in boosters)
                    if (b != null) _boosters[b.Type] = b;
        }

        public async UniTask<BoosterResult> UseAsync(BoosterType type, CancellationToken ct = default)
        {
            if (IsBusy)
                return BoosterResult.Rejected(type, "busy");

            if (!_boosters.TryGetValue(type, out IBooster booster))
                return BoosterResult.Rejected(type, "not registered");

            if (_inventory.Count(type) <= 0 || !booster.CanUse(_ctx.Session))
                return BoosterResult.Rejected(type, "unavailable");

            IsBusy = true;
            BoosterStarted?.Invoke(type);
            _gate?.SetMode(BoardInputMode.Locked); 

            BoosterResult result;
            try
            {
                result = await booster.UseAsync(_ctx, ct);
                if (result.Applied)
                    _inventory.TrySpend(type);
            }
            finally
            {
                _gate?.SetMode(BoardInputMode.Play); 
                IsBusy = false;
            }

            BoosterFinished?.Invoke(result);
            return result;
        }
    }
}
