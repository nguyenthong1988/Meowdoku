using Cast.Game;

namespace Cast.Game
{

    public sealed class BoardState
    {
        private readonly PlayerMark[,] _marks;

        public LevelData Level { get; }
        public int Size { get; }

        public int CatsRevealed { get; private set; }

        public BoardState(LevelData level)
        {
            Level = level;
            Size = level.Size;
            _marks = new PlayerMark[Size, Size];
        }

        public PlayerMark GetMark(int row, int col) => _marks[row, col];

        public void SetMark(int row, int col, PlayerMark mark)
        {
            PlayerMark prev = _marks[row, col];
            if (prev == mark) return;

            if (prev == PlayerMark.Character) CatsRevealed--;
            if (mark == PlayerMark.Character) CatsRevealed++;
            _marks[row, col] = mark;
        }

        public bool InBounds(int row, int col) =>
            row >= 0 && row < Size && col >= 0 && col < Size;

        public bool IsSolved()
        {
            
            if (CatsRevealed != Level.Solution.Count) return false;
            for (int i = 0; i < Level.Solution.Count; i++)
            {
                CatPlacement c = Level.Solution[i];
                if (_marks[c.Row, c.Col] != PlayerMark.Character) return false;
            }
            return true;
        }

        public void Reset()
        {
            for (int r = 0; r < Size; r++)
                for (int c = 0; c < Size; c++)
                    _marks[r, c] = PlayerMark.None;
            CatsRevealed = 0;
        }
    }
}
