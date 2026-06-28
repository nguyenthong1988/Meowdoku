namespace Cast.Game
{

    public readonly struct MoveRecord
    {
        public readonly int Row;
        public readonly int Col;
        public readonly PlayerMark From;
        public readonly PlayerMark To;
        public readonly bool CostHeart;

        public MoveRecord(int row, int col, PlayerMark from, PlayerMark to, bool costHeart)
        {
            Row = row;
            Col = col;
            From = from;
            To = to;
            CostHeart = costHeart;
        }
    }
}
