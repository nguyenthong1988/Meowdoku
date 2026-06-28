namespace Cast.Game
{

    public interface IBoardInput
    {
        BoardInputMode Mode { get; }
        void SetMode(BoardInputMode mode);
    }
}
