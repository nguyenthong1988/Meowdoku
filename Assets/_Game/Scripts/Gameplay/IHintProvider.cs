namespace Cast.Game.Gameplay
{

    public interface IHintProvider
    {
        
        bool TryGetHint(BoardState state, out int row, out int col);
    }
}
