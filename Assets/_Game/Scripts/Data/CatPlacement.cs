namespace Cast.Game
{

    public readonly struct CatPlacement
    {
        public readonly int Row;
        public readonly int Col;
        public readonly sbyte ColorIndex;

        public CatPlacement(int row, int col, sbyte colorIndex)
        {
            Row = row;
            Col = col;
            ColorIndex = colorIndex;
        }
    }
}
