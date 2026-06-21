namespace Cast.Game.Gameplay
{

    public readonly struct CellChange
    {
        public readonly int Row;
        public readonly int Col;
        public readonly PlayerMark From;
        public readonly PlayerMark To;

        public CellChange(int row, int col, PlayerMark from, PlayerMark to)
        {
            Row = row;
            Col = col;
            From = from;
            To = to;
        }
    }
}
