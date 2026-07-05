namespace Cast.Game
{

    public readonly struct CharacterPlacement
    {
        public readonly int Row;
        public readonly int Col;
        public readonly sbyte ColorIndex;

        public CharacterPlacement(int row, int col, sbyte colorIndex)
        {
            Row = row;
            Col = col;
            ColorIndex = colorIndex;
        }
    }
}
