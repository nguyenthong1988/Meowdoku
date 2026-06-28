namespace Cast.Game
{

    public readonly struct CellData
    {
        
        public const sbyte EmptyColor = -1;

        public readonly sbyte ColorIndex;

        public readonly bool HasCat;

        public CellData(sbyte colorIndex, bool hasCat)
        {
            ColorIndex = colorIndex;
            HasCat = hasCat;
        }

        public bool IsFilled => ColorIndex != EmptyColor;

        public static readonly CellData Empty = new CellData(EmptyColor, false);
    }
}
