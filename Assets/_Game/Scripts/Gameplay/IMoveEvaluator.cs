namespace Cast.Game.Gameplay
{

    public interface IMoveEvaluator
    {
        
        bool IsLegalPlacement(BoardState state, int row, int col);

        bool IsSolutionCell(BoardState state, int row, int col);

        MoveOutcome EvaluatePlacement(BoardState state, int row, int col);
    }
}
