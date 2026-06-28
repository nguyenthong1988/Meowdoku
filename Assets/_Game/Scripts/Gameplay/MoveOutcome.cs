namespace Cast.Game
{

    public enum MoveOutcome : byte
    {
        NoOp = 0,

        Revealed = 1,

        Wrong = 2,

        Hinted = 3,

        Unhinted = 4,
    }
}
