using Cast.Game.Board;
using Cast.Game.Gameplay;

namespace Cast.Game.Booster
{

    public sealed class BoosterContext
    {
        public IGameSession Session { get; }
        public BoardView Board { get; }
        public IBoardTargeting Targeting { get; }

        public BoosterContext(IGameSession session, BoardView board, IBoardTargeting targeting)
        {
            Session = session;
            Board = board;
            Targeting = targeting;
        }
    }
}
