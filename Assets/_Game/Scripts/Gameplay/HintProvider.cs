using System.Collections.Generic;
using Cast.Game;

namespace Cast.Game
{

    public sealed class HintProvider : IHintProvider
    {
        public bool TryGetHint(BoardState state, out int row, out int col)
        {
            IReadOnlyList<CatPlacement> solution = state.Level.Solution;
            for (int i = 0; i < solution.Count; i++)
            {
                CatPlacement c = solution[i];
                if (state.GetMark(c.Row, c.Col) != PlayerMark.Cat)
                {
                    row = c.Row;
                    col = c.Col;
                    return true;
                }
            }

            row = -1;
            col = -1;
            return false;
        }
    }
}
