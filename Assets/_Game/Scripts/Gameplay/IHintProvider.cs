namespace Cast.Game
{

    public interface IHintProvider
    {
        
        bool TryGetHint(BoardState state, out int row, out int col);
    }
}
