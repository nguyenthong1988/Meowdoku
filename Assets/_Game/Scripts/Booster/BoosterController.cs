using System;
using System.Collections.Generic;
using CaskFramework.UI;

namespace Cast.Game
{
    public sealed class BoosterController : IBoosterController
    {
        private readonly IBoardInput _input;
        private readonly IBoosterInventory _inventory;
        private readonly Dictionary<BoosterType, IBooster> _boosters = new Dictionary<BoosterType, IBooster>();

        public IGameSession Session { get; }
        public BoardView Board { get; }
        public IBoardTargeting Targeting { get; }
        public IBoardInput Input => _input;
        public IUIManager Ui { get; }
        public bool IsBusy { get; private set; }

        public event Action<BoosterType> BoosterStarted;
        public event Action<BoosterResult> BoosterFinished;

        public BoosterController(IGameSession session, IBoardInput input, IBoardTargeting targeting,
                                 BoardView board, IBoosterInventory inventory, IUIManager ui, params IBooster[] boosters)
        {
            Session = session ?? throw new ArgumentNullException(nameof(session));
            _input = input ?? throw new ArgumentNullException(nameof(input));
            Targeting = targeting;
            Board = board;
            _inventory = inventory ?? throw new ArgumentNullException(nameof(inventory));
            Ui = ui ?? throw new ArgumentNullException(nameof(ui));

            if (boosters != null)
                foreach (IBooster b in boosters)
                    if (b != null) _boosters[b.Type] = b;
        }

        public void Use(BoosterType type, Action<BoosterResult> onDone = null)
        {
            if (IsBusy)
            {
                onDone?.Invoke(BoosterResult.Rejected(type, "busy"));
                return;
            }

            if (!_boosters.TryGetValue(type, out IBooster booster))
            {
                onDone?.Invoke(BoosterResult.Rejected(type, "not registered"));
                return;
            }

            IsBusy = true;
            BoosterStarted?.Invoke(type);
            _input?.SetMode(BoardInputMode.Locked);

            bool completed = false;

            void Complete(BoosterResult result)
            {
                if (completed) return;
                completed = true;

                if (result.Applied)
                    _inventory.TrySpend(type);

                _input?.SetMode(BoardInputMode.Play);
                IsBusy = false;

                BoosterFinished?.Invoke(result);
                onDone?.Invoke(result);
            }

            try
            {
                booster.Execute(this, Complete);
            }
            catch (Exception)
            {
                Complete(BoosterResult.Rejected(type, "error"));
                throw;
            }
        }
    }
}
