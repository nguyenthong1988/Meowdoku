namespace Cast.Game.Board
{

    public interface IBoardInputGate
    {
        BoardInputMode Mode { get; }
        void SetMode(BoardInputMode mode);
    }
}
