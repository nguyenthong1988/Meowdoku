using System;
using Cast.Game.Data;

namespace Cast.Game.Gameplay
{

    public sealed class MoveEvaluator : IMoveEvaluator
    {
        public bool IsLegalPlacement(BoardState state, int row, int col)
        {
            CellData target = state.Level.GetCell(row, col);
            if (!target.IsFilled) return false;                 
            if (state.GetMark(row, col) == PlayerMark.Cat) return false; 

            int size = state.Size;
            for (int r = 0; r < size; r++)
            {
                for (int c = 0; c < size; c++)
                {
                    if (state.GetMark(r, c) != PlayerMark.Cat) continue;
                    if (r == row && c == col) continue;

                    if (r == row) return false;                 
                    if (c == col) return false;                 
                    if (Touches(row, col, r, c)) return false;  

                    if (state.Level.GetCell(r, c).ColorIndex == target.ColorIndex) return false;
                }
            }
            return true;
        }

        public bool IsSolutionCell(BoardState state, int row, int col)
        {
            
            CellData cell = state.Level.GetCell(row, col);
            return cell.HasCat;
        }

        public MoveOutcome EvaluatePlacement(BoardState state, int row, int col)
        {
            if (!IsLegalPlacement(state, row, col))
                return MoveOutcome.RejectedIllegal;

            return IsSolutionCell(state, row, col)
                ? MoveOutcome.Placed
                : MoveOutcome.Wrong;
        }

        private static bool Touches(int r1, int c1, int r2, int c2) =>
            Math.Abs(r1 - r2) <= 1 && Math.Abs(c1 - c2) <= 1;
    }
}
