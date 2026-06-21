namespace Cast.Game.Board
{
    
    public enum PointerGesture : byte
    {
        Tap = 0,
        DoubleTap = 1,
        LongPress = 2,
    }

    public readonly struct CellGesture
    {
        public readonly int Row;
        public readonly int Col;
        public readonly PointerGesture Gesture;

        public CellGesture(int row, int col, PointerGesture gesture)
        {
            Row = row;
            Col = col;
            Gesture = gesture;
        }
    }
}
