using System;
using System.Collections.Generic;
using System.Threading;


using Cysharp.Threading.Tasks;

namespace Cast.Game
{

    public sealed class BoosterController : IBoosterController
    {
        private readonly IBoardInput _gate;
        private readonly IBoosterInventory _inventory;
        private readonly Dictionary<BoosterType, IBooster> _boosters = new Dictionary<BoosterType, IBooster>();

        public IGameSession Session { get; }
        public BoardView Board { get; }
        public IBoardTargeting Targeting { get; }
        public bool IsBusy { get; private set; }

        public event Action<BoosterType> BoosterStarted;
        public event Action<BoosterResult> BoosterFinished;

        public BoosterController(IGameSession session, IBoardInput gate, IBoardTargeting targeting,
                                 BoardView board, IBoosterInventory inventory, params IBooster[] boosters)
        {
            Session = session ?? throw new ArgumentNullException(nameof(session));
            _gate = gate ?? throw new ArgumentNullException(nameof(gate));
            Targeting = targeting;
            Board = board;
            _inventory = inventory ?? throw new ArgumentNullException(nameof(inventory));

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

            // if (_inventory.Count(type) <= 0 || !booster.CanUse(Session))
            //     return BoosterResult.Rejected(type, "unavailable");

            IsBusy = true;
            BoosterStarted?.Invoke(type);
            _gate?.SetMode(BoardInputMode.Locked);

            BoosterResult result;
            try
            {
                result = await booster.UseAsync(this, ct);
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
